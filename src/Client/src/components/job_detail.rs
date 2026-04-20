use crossterm::event::{KeyCode, KeyEventKind};
use ratatui::{
    text::{Line, Text},
    widgets::{Block, Borders, Paragraph},
};
use tokio::sync::mpsc;
use tokio_stream::StreamExt;

use crate::{action::Action, client::CommanderClient, components::Component};

pub struct JobDetail {
    pub job_id: String,
    logs: Vec<String>,
    log_rx: mpsc::UnboundedReceiver<String>,
}

impl JobDetail {
    pub fn new(job_id: String, mut client: CommanderClient) -> Self {
        let (tx, rx) = mpsc::unbounded_channel();
        let id = job_id.clone();

        tokio::spawn(async move {
            let mut stream = client.monitor_job(id).await.expect("monitor_job failed");

            while let Some(Ok(msg)) = stream.next().await {
                let prefix = if msg.is_error { "[ERR] " } else { "[OUT] " };
                let _ = tx.send(format!("{}{}", prefix, msg.log));

                if msg.is_final {
                    break;
                }
            }
        });

        Self {
            job_id,
            logs: Vec::new(),
            log_rx: rx,
        }
    }
}

impl Component for JobDetail {
    fn render(&mut self, f: &mut ratatui::Frame, rect: ratatui::prelude::Rect) {
        while let Ok(line) = self.log_rx.try_recv() {
            self.logs.push(line);
        }

        let text: Vec<Line> = self.logs.iter().map(|l| Line::from(l.as_str())).collect();
        let paragraph = Paragraph::new(Text::from(text)).block(
            Block::default()
                .title(format!("Job: {}", self.job_id))
                .borders(Borders::ALL),
        );

        f.render_widget(paragraph, rect);
    }

    fn handle_key_events(&mut self, key: crossterm::event::KeyEvent) -> crate::action::Action {
        if key.kind != KeyEventKind::Press {
            return Action::Ignore;
        }

        match key.code {
            KeyCode::Backspace => Action::OpenJobList,
            _ => Action::Ignore,
        }
    }
}
