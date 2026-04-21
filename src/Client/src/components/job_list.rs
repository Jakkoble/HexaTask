use crossterm::event::{KeyCode, KeyEventKind};
use ratatui::{
    layout::{Constraint, Layout},
    style::{Color, Style},
    text::Text,
    widgets::{Block, List, ListState, Paragraph},
};

use crate::{action::Action, app::Job, components::Component};

pub struct JobList {
    pub jobs: Vec<Job>,
    pub list_state: ListState,
}

impl JobList {
    pub fn new(jobs: Vec<Job>) -> Self {
        Self {
            jobs,
            list_state: ListState::default().with_selected(Some(0)),
        }
    }
}

impl Component for JobList {
    fn render(&mut self, f: &mut ratatui::Frame, rect: ratatui::prelude::Rect) {
        let chunks = Layout::default()
            .direction(ratatui::layout::Direction::Horizontal)
            .constraints([Constraint::Percentage(50), Constraint::Percentage(50)])
            .split(rect);

        let items = self
            .jobs
            .iter()
            .map(|job| Text::raw(job.name.clone()))
            .collect::<Vec<_>>();

        let list = List::new(items)
            .style(Color::White)
            .highlight_style(Style::new().cyan().italic())
            .highlight_symbol("> ")
            .scroll_padding(1);

        f.render_stateful_widget(list, chunks[0], &mut self.list_state);

        if let Some(index) = self.list_state.selected() {
            let content = match self.jobs.get(index) {
                Some(job) => job.raw.clone(),
                None => "Job not found".to_string(),
            };

            let paragraph =
                Paragraph::new(content).block(Block::bordered().title_top("Selected job"));

            f.render_widget(paragraph, chunks[1]);
        }
    }

    fn handle_key_events(&mut self, key: crossterm::event::KeyEvent) -> Option<Action> {
        if key.kind != KeyEventKind::Press {
            return None;
        }

        match key.code {
            KeyCode::Char('j') | KeyCode::Down => {
                self.list_state.select_next();
                None
            }
            KeyCode::Char('k') | KeyCode::Up => {
                self.list_state.select_previous();
                None
            }
            KeyCode::Char('q') | KeyCode::Esc => Some(Action::Quit),
            KeyCode::Enter => {
                if let Some(index) = self.list_state.selected() {
                    if let Some(job) = self.jobs.get(index) {
                        return Some(Action::SelectJob(job.clone()));
                    }
                }

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

    fn job(name: &str) -> Job {
        Job {
            name: name.to_string(),
            raw: format!("raw-{name}"),
        }
    }

    fn key_event(code: KeyCode) -> KeyEvent {
        KeyEvent {
            code,
            modifiers: KeyModifiers::NONE,
            kind: KeyEventKind::Press,
            state: KeyEventState::NONE,
        }
    }

    #[test]
    fn enter_selects_current_job() {
        let mut list = JobList::new(vec![job("a.yaml"), job("b.yaml")]);
        list.list_state.select(Some(1));

        let action = list.handle_key_events(key_event(KeyCode::Enter));

        match action {
            Some(Action::SelectJob(job)) => assert_eq!(job.name, "b.yaml"),
            _ => panic!("expected selected job action"),
        }
    }

    #[test]
    fn navigation_updates_selected_index() {
        let mut list = JobList::new(vec![job("a.yaml"), job("b.yaml")]);

        list.handle_key_events(key_event(KeyCode::Down));
        assert_eq!(list.list_state.selected(), Some(1));

        list.handle_key_events(key_event(KeyCode::Up));
        assert_eq!(list.list_state.selected(), Some(0));
    }

    #[test]
    fn quit_keys_return_quit_action() {
        let mut list = JobList::new(vec![job("a.yaml")]);

        assert!(matches!(
            list.handle_key_events(key_event(KeyCode::Char('q'))),
            Some(Action::Quit)
        ));
        assert!(matches!(
            list.handle_key_events(key_event(KeyCode::Esc)),
            Some(Action::Quit)
        ));
    }
}
