use crossterm::event::{Event, KeyEvent};
use ratatui::{Frame, layout::Rect};

use crate::action::Action;

pub mod job_detail;
pub mod job_list;

pub trait Component {
    fn handle_events(&mut self, event: Option<Event>) -> Action {
        match event {
            Some(Event::Key(key_event)) => self.handle_key_events(key_event),
            None => Action::Ignore,
            _ => Action::Ignore,
        }
    }

    fn handle_key_events(&mut self, _key: KeyEvent) -> Action {
        Action::Ignore
    }

    fn render(&mut self, f: &mut Frame, rect: Rect);
}
