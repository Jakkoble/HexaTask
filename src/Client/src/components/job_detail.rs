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
    scroll_offset: usize,
    auto_follow: bool,
    visible_log_lines: usize,
}

impl JobDetail {
    pub(crate) fn new(job_id: String, log_rx: JobLogReceiver) -> Self {
        Self {
            job_id,
            logs: Vec::new(),
            log_rx,
            scroll_offset: 0,
            auto_follow: true,
            visible_log_lines: 0,
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

        let running_status = if self.log_rx.is_closed() {
            Span::styled("Finished", Style::new().yellow())
        } else {
            Span::styled(" Running ", Style::new().on_yellow().black())
        };

        let top_line = Line::from(vec![
            Span::raw("Job ID: "),
            Span::styled(&self.job_id, Style::new().blue()),
            Span::raw("   "),
            Span::raw("Status: "),
            running_status,
            Span::raw("   "),
            Span::raw("Log Lines: "),
            Span::styled(self.logs.len().to_string(), Style::new().green()),
        ]);
        let top_block =
            Paragraph::new(top_line).block(Block::bordered().title_top(" Job Details "));
        f.render_widget(top_block, top_area);

        self.visible_log_lines = log_area.height.saturating_sub(2) as usize;
        let max_scroll_offset = self.logs.len().saturating_sub(self.visible_log_lines);

        self.scroll_offset = if self.auto_follow {
            max_scroll_offset
        } else {
            self.scroll_offset.min(max_scroll_offset)
        };

        let logs: Vec<Line> = self.logs.iter().map(|l| Line::from(l.as_str())).collect();

        let paragraph = Paragraph::new(Text::from(logs))
            .scroll((self.scroll_offset as u16, 0))
            .block(
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
            Span::styled(" Scroll ", Style::new().black().on_yellow()),
            Span::raw(" "),
            Span::styled("j/k", Style::new().yellow()),
            Span::raw(" or "),
            Span::styled("↑/↓", Style::new().yellow()),
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
            KeyCode::Char('k') | KeyCode::Up => {
                self.scroll_offset = self.scroll_offset.saturating_sub(1);
                self.auto_follow = false;
                None
            }
            KeyCode::Char('j') | KeyCode::Down => {
                let max_scroll_offset = self.logs.len().saturating_sub(self.visible_log_lines);
                self.scroll_offset = (self.scroll_offset + 1).min(max_scroll_offset);
                self.auto_follow = self.scroll_offset == max_scroll_offset;
                None
            }
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

        let backend = TestBackend::new(140, 12);
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
        assert!(rendered.contains("Status:"));
        assert!(rendered.contains("Log Lines:"));
        assert!(rendered.contains("2"));
        assert!(rendered.contains("[OUT] started"));
        assert!(rendered.contains("[ERR] failed"));
        assert!(rendered.contains("Backspace"));
        assert!(rendered.contains("j/k"));
    }

    #[test]
    fn scroll_keys_update_offset() {
        let (_tx, rx) = mpsc::unbounded_channel();
        let mut detail = JobDetail::new("job-1".to_string(), rx);
        detail.logs = (0..10).map(|i| format!("line-{i}")).collect();
        detail.visible_log_lines = 3;
        detail.scroll_offset = 7;
        detail.auto_follow = true;

        detail.handle_key_events(key_event(KeyCode::Up));
        assert_eq!(detail.scroll_offset, 6);
        assert!(!detail.auto_follow);

        detail.handle_key_events(key_event(KeyCode::Char('j')));
        assert_eq!(detail.scroll_offset, 7);
        assert!(detail.auto_follow);
    }
}
