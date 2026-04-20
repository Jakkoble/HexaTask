use crate::app::Job;

pub enum Action {
    Ignore,
    Quit,
    SelectJob(Job),
    OpenJobList,
}
