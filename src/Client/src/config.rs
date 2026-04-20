use std::env;

pub struct Config {
    pub commander_addr: String,
    pub job_dir: String,
}

impl Config {
    pub fn from_env() -> Self {
        Self {
            commander_addr: env::var("COMMANDER_ADDR")
                .expect("COMMANDER_ADDR environmental variable to be set."),
            job_dir: env::var("JOB_DIR").unwrap_or("jobs".to_string()),
        }
    }
}
