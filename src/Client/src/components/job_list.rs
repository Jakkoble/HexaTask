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

    fn handle_key_events(&mut self, key: crossterm::event::KeyEvent) -> crate::action::Action {
        if key.kind != KeyEventKind::Press {
            return Action::Ignore;
        }

        match key.code {
            KeyCode::Char('j') | KeyCode::Down => {
                self.list_state.select_next();
                Action::Ignore
            }
            KeyCode::Char('k') | KeyCode::Up => {
                self.list_state.select_previous();
                Action::Ignore
            }
            KeyCode::Char('q') | KeyCode::Esc => Action::Quit,
            KeyCode::Enter => {
                if let Some(index) = self.list_state.selected() {
                    if let Some(job) = self.jobs.get(index) {
                        return Action::SelectJob(job.clone());
                    }
                }

                Action::Ignore
            }
            _ => Action::Ignore,
        }
    }
}
