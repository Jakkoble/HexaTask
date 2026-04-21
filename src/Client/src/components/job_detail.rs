use crossterm::event::{KeyCode, KeyEventKind};
use ratatui::{
    layout::{Constraint, Layout},
    style::Style,
    text::{Line, Span, Text},
    widgets::{Block, Borders, Paragraph},
};

use crate::{action::Action, client::JobLogReceiver, components::Component};

pub(crate) struct JobDetail {
    job_id: String,
    logs: Vec<String>,
    log_rx: JobLogReceiver,
}

impl JobDetail {
    pub(crate) fn new(job_id: String, log_rx: JobLogReceiver) -> Self {
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

        let [top_area, log_area, controll_area] = Layout::vertical([
            Constraint::Length(3),
            Constraint::Min(3),
            Constraint::Length(3),
        ])
        .areas(rect);

        let top_line = Line::from(vec![
            Span::raw("Job ID: "),
            Span::styled(&self.job_id, Style::new().blue()),
            Span::raw("   "),
            Span::raw("Log Lines: "),
            Span::styled(self.logs.len().to_string(), Style::new().green()),
        ]);

        let top_block =
            Paragraph::new(top_line).block(Block::bordered().title_top(" Job Details "));
        f.render_widget(top_block, top_area);

        let logs: Vec<Line> = self.logs.iter().map(|l| Line::from(l.as_str())).collect();
        let paragraph = Paragraph::new(Text::from(logs)).block(
            Block::default()
                .title_top(" Live Logs ")
                .borders(Borders::ALL),
        );
        f.render_widget(paragraph, log_area);

        let control_line = Line::from(vec![
            Span::styled(" Back ", Style::new().black().on_cyan()),
            Span::raw(" "),
            Span::styled("Backspace", Style::new().cyan()),
            Span::raw("   "),
            Span::styled(" Quit ", Style::new().black().on_red()),
            Span::raw(" "),
            Span::styled("q", Style::new().red()),
            Span::raw("/"),
            Span::styled("Esc", Style::new().red()),
        ]);
        let control_block =
            Paragraph::new(control_line).block(Block::bordered().title_top(" Controls "));

        f.render_widget(control_block, controll_area);
    }

    fn handle_key_events(&mut self, key: crossterm::event::KeyEvent) -> Option<Action> {
        if key.kind != KeyEventKind::Press {
            return None;
        }

        match key.code {
            KeyCode::Char('q') | KeyCode::Esc => Some(Action::Quit),
            KeyCode::Backspace => Some(Action::OpenJobList),
            _ => None,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crossterm::event::{KeyEvent, KeyEventState, KeyModifiers};
    use ratatui::{Terminal, backend::TestBackend};
    use tokio::sync::mpsc;

    fn key_event(code: KeyCode) -> KeyEvent {
        KeyEvent {
            code,
            modifiers: KeyModifiers::NONE,
            kind: KeyEventKind::Press,
            state: KeyEventState::NONE,
        }
    }

    #[test]
    fn backspace_opens_job_list() {
        let (_tx, rx) = mpsc::unbounded_channel();
        let mut detail = JobDetail::new("job-1".to_string(), rx);

        let action = detail.handle_key_events(key_event(KeyCode::Backspace));

        assert!(matches!(action, Some(Action::OpenJobList)));
    }

    #[test]
    fn render_collects_available_log_lines() {
        let (tx, rx) = mpsc::unbounded_channel();
        tx.send("[OUT] started".to_string())
            .expect("first log line should be queued");
        tx.send("[ERR] failed".to_string())
            .expect("second log line should be queued");

        let backend = TestBackend::new(100, 12);
        let mut terminal = Terminal::new(backend).expect("test terminal should be created");
        let mut detail = JobDetail::new("job-1".to_string(), rx);

        terminal
            .draw(|frame| detail.render(frame, frame.area()))
            .expect("detail view should render");

        let buffer = terminal.backend().buffer().clone();
        let rendered = buffer
            .content()
            .iter()
            .map(|cell| cell.symbol())
            .collect::<String>();

        assert!(rendered.contains("Job ID:"));
        assert!(rendered.contains("job-1"));
        assert!(rendered.contains("Log Lines:"));
        assert!(rendered.contains("2"));
        assert!(rendered.contains("[OUT] started"));
        assert!(rendered.contains("[ERR] failed"));
        assert!(rendered.contains("Backspace"));
    }
}
