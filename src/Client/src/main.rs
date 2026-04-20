use std::error::Error;

use crate::{app::App, config::Config};

mod action;
mod app;
mod client;
mod components;
mod config;

#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    let config = Config::from_env();

    let mut app = App::new(config);
    let mut terminal = ratatui::init();

    let result = app.run(&mut terminal).await;

    ratatui::restore();
    result
}
