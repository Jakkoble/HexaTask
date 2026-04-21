use std::{fs, time::Duration};

use crossterm::event;

use crate::{
    action::Action,
    client::{ClientError, CommanderClientApi},
    components::{Component, job_detail::JobDetail, job_list::JobList},
    config::Config,
};

#[derive(Clone)]
pub struct Job {
    pub name: String,
    pub raw: String,
}

pub struct App {
    should_quit: bool,
    config: Config,
    pub screen: Box<dyn Component>,
    pub client: Box<dyn CommanderClientApi>,
}

impl App {
    pub fn new(config: Config, client: Box<dyn CommanderClientApi>) -> Self {
        let jobs = Self::get_jobs(&config.job_dir);

        Self {
            config,
            should_quit: false,
            screen: Box::new(JobList::new(jobs)),
            client,
        }
    }

    pub async fn run(
        &mut self,
        terminal: &mut ratatui::DefaultTerminal,
    ) -> Result<(), ClientError> {
        while !self.should_quit {
            terminal.draw(|frame| self.screen.render(frame, frame.area()))?;
            self.handle_events().await?;
        }

        Ok(())
    }

    async fn handle_events(&mut self) -> Result<(), ClientError> {
        let event = if event::poll(Duration::from_millis(100))? {
            Some(event::read()?)
        } else {
            None
        };

        let Some(action) = self.screen.handle_events(event) else {
            return Ok(());
        };

        match action {
            Action::Quit => self.should_quit = true,
            Action::SelectJob(job) => {
                let job_id = self.client.submit_job(job.raw).await?;
                let log_rx = self.client.monitor_job(job_id.clone()).await?;

                self.screen = Box::new(JobDetail::new(job_id, log_rx));
            }
            Action::OpenJobList => {
                let jobs = Self::get_jobs(&self.config.job_dir);

                self.screen = Box::new(JobList::new(jobs));
            }
        }

        Ok(())
    }

    fn get_jobs(job_dir: &str) -> Vec<Job> {
        let Ok(entries) = fs::read_dir(job_dir) else {
            return Vec::new();
        };

        entries
            .filter_map(|entry| {
                let item = entry.ok()?;
                let file_type = item.file_type().ok()?;

                if !file_type.is_file() {
                    return None;
                }

                Some(Job {
                    name: item.file_name().into_string().ok()?,
                    raw: fs::read_to_string(item.path()).ok()?,
                })
            })
            .collect()
    }
}
