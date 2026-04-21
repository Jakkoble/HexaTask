use crossterm::event::{KeyCode, KeyEventKind};
use ratatui::{
    text::{Line, Text},
    widgets::{Block, Borders, Paragraph},
};

use crate::{action::Action, client::JobLogReceiver, components::Component};

pub struct JobDetail {
    pub job_id: String,
    logs: Vec<String>,
    log_rx: JobLogReceiver,
}

impl JobDetail {
    pub fn new(job_id: String, log_rx: JobLogReceiver) -> Self {
        Self {
            job_id,
            logs: Vec::new(),
            log_rx,
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

    fn handle_key_events(&mut self, key: crossterm::event::KeyEvent) -> Option<Action> {
        if key.kind != KeyEventKind::Press {
            return None;
        }

        match key.code {
            KeyCode::Backspace => Some(Action::OpenJobList),
            _ => None,
        }
    }
}
