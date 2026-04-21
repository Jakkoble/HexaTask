use crossterm::event::{Event, KeyEvent};
use ratatui::{Frame, layout::Rect};

use crate::action::Action;

pub mod job_detail;
pub mod job_list;

pub trait Component {
    fn handle_events(&mut self, event: Option<Event>) -> Option<Action> {
        match event {
            Some(Event::Key(key_event)) => self.handle_key_events(key_event),
            _ => None,
        }
    }

    fn handle_key_events(&mut self, _key: KeyEvent) -> Option<Action> {
        None
    }

    fn render(&mut self, f: &mut Frame, rect: Rect);
}
