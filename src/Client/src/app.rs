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

        self.apply_action(action).await
    }

    async fn apply_action(&mut self, action: Action) -> Result<(), ClientError> {
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

#[cfg(test)]
mod tests {
    use super::*;
    use async_trait::async_trait;
    use crossterm::event::{Event, KeyCode, KeyEvent, KeyEventKind, KeyEventState, KeyModifiers};
    use ratatui::{Frame, layout::Rect};
    use std::{
        path::PathBuf,
        sync::{Arc, Mutex},
        time::{SystemTime, UNIX_EPOCH},
    };
    use tokio::sync::mpsc;

    use crate::{client::JobLogReceiver, components::Component};

    #[derive(Default)]
    struct MockClientState {
        submitted_payloads: Vec<String>,
        monitored_job_ids: Vec<String>,
    }

    struct MockClient {
        state: Arc<Mutex<MockClientState>>,
    }

    #[async_trait]
    impl CommanderClientApi for MockClient {
        async fn submit_job(&self, yaml_payload: String) -> Result<String, ClientError> {
            self.state
                .lock()
                .expect("mock client state should not be poisoned")
                .submitted_payloads
                .push(yaml_payload);

            Ok("job-123".to_string())
        }

        async fn monitor_job(&self, job_id: String) -> Result<JobLogReceiver, ClientError> {
            self.state
                .lock()
                .expect("mock client state should not be poisoned")
                .monitored_job_ids
                .push(job_id);

            let (_tx, rx) = mpsc::unbounded_channel();
            Ok(rx)
        }
    }

    fn test_config(job_dir: String) -> Config {
        Config {
            commander_addr: "http://localhost:50051".to_string(),
            job_dir,
        }
    }

    fn temp_dir(prefix: &str) -> PathBuf {
        let unique = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .expect("time should be after unix epoch")
            .as_nanos();
        let path = std::env::temp_dir().join(format!("{prefix}-{unique}"));

        fs::create_dir_all(&path).expect("temp dir should be created");
        path
    }

    fn key_event(code: KeyCode) -> KeyEvent {
        KeyEvent {
            code,
            modifiers: KeyModifiers::NONE,
            kind: KeyEventKind::Press,
            state: KeyEventState::NONE,
        }
    }

    struct StaticActionComponent {
        action: Option<Action>,
    }

    impl Component for StaticActionComponent {
        fn handle_events(&mut self, _event: Option<Event>) -> Option<Action> {
            self.action.take()
        }

        fn render(&mut self, _f: &mut Frame, _rect: Rect) {}
    }

    #[test]
    fn get_jobs_returns_only_regular_files() {
        let dir = temp_dir("hexa-client-jobs");
        let file_path = dir.join("job-a.yaml");
        let nested_dir = dir.join("nested");

        fs::write(&file_path, "name: a").expect("job file should be written");
        fs::create_dir(&nested_dir).expect("nested dir should be created");

        let jobs = App::get_jobs(dir.to_str().expect("temp path should be valid utf-8"));

        assert_eq!(jobs.len(), 1);
        assert_eq!(jobs[0].name, "job-a.yaml");
        assert_eq!(jobs[0].raw, "name: a");

        fs::remove_dir_all(dir).expect("temp dir should be removed");
    }

    #[tokio::test]
    async fn apply_action_quit_marks_app_for_exit() {
        let state = Arc::new(Mutex::new(MockClientState::default()));
        let client = Box::new(MockClient { state });
        let mut app = App::new(test_config("missing".to_string()), client);

        app.apply_action(Action::Quit)
            .await
            .expect("quit action should succeed");

        assert!(app.should_quit);
    }

    #[tokio::test]
    async fn apply_action_select_job_submits_and_switches_to_detail_screen() {
        let state = Arc::new(Mutex::new(MockClientState::default()));
        let client = Box::new(MockClient {
            state: Arc::clone(&state),
        });
        let mut app = App::new(test_config("missing".to_string()), client);

        app.apply_action(Action::SelectJob(Job {
            name: "job.yaml".to_string(),
            raw: "kind: job".to_string(),
        }))
        .await
        .expect("select job action should succeed");

        let state = state
            .lock()
            .expect("mock client state should not be poisoned");
        assert_eq!(state.submitted_payloads, vec!["kind: job".to_string()]);
        assert_eq!(state.monitored_job_ids, vec!["job-123".to_string()]);
        drop(state);

        let action = app
            .screen
            .handle_events(Some(Event::Key(key_event(KeyCode::Backspace))));
        assert!(matches!(action, Some(Action::OpenJobList)));
    }

    #[tokio::test]
    async fn apply_action_open_job_list_reloads_jobs() {
        let dir = temp_dir("hexa-client-open-list");
        fs::write(dir.join("job-b.yaml"), "kind: batch").expect("job file should be written");

        let state = Arc::new(Mutex::new(MockClientState::default()));
        let client = Box::new(MockClient { state });
        let mut app = App::new(test_config(dir.to_string_lossy().into_owned()), client);
        app.screen = Box::new(StaticActionComponent { action: None });

        app.apply_action(Action::OpenJobList)
            .await
            .expect("open job list action should succeed");

        let action = app
            .screen
            .handle_events(Some(Event::Key(key_event(KeyCode::Enter))));

        match action {
            Some(Action::SelectJob(job)) => {
                assert_eq!(job.name, "job-b.yaml");
                assert_eq!(job.raw, "kind: batch");
            }
            _ => panic!("expected selected job action from reloaded job list"),
        }

        fs::remove_dir_all(dir).expect("temp dir should be removed");
    }
}
