use crate::app::Job;

pub enum Action {
    Quit,
    SelectJob(Job),
    OpenJobList,
}
