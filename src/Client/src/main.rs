use crossterm::{
    event::{KeyboardEnhancementFlags, PopKeyboardEnhancementFlags, PushKeyboardEnhancementFlags},
    execute,
    terminal::supports_keyboard_enhancement,
};

use crate::{app::App, client::CommanderClient, config::Config};

mod action;
mod app;
mod client;
mod components;
mod config;

#[tokio::main]
async fn main() {
    if let Err(err) = run().await {
        eprintln!("{err}");
        std::process::exit(1);
    }
}

async fn run() -> Result<(), client::ClientError> {
    let config = Config::from_env();
    let client = Box::new(CommanderClient::connect(&config.commander_addr).await?);

    let mut app = App::new(config, client);
    let mut terminal = ratatui::init();

    let enhanced = supports_keyboard_enhancement().unwrap_or(false);
    if enhanced {
        execute!(
            std::io::stdout(),
            PushKeyboardEnhancementFlags(KeyboardEnhancementFlags::DISAMBIGUATE_ESCAPE_CODES)
        )
        .unwrap_or(());
    }

    let result = app.run(&mut terminal).await;

    if enhanced {
        execute!(std::io::stdout(), PopKeyboardEnhancementFlags).unwrap_or(());
    }

    ratatui::restore();
    result
}
