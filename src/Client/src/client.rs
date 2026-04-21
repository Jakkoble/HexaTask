mod orchestrator {
    tonic::include_proto!("orchestrator");
}

use crate::client::orchestrator::MonitorJobRequest;
use async_trait::async_trait;
use orchestrator::{SubmitJobRequest, orchestrator_service_client::OrchestratorServiceClient};
use std::error::Error;
use tokio::sync::mpsc;
use tokio_stream::StreamExt;
use tonic::transport::Channel;

pub(crate) type ClientError = Box<dyn Error>;
pub(crate) type JobLogReceiver = mpsc::UnboundedReceiver<String>;

#[async_trait]
pub(crate) trait CommanderClientApi {
    async fn submit_job(&self, yaml_payload: String) -> Result<String, ClientError>;
    async fn monitor_job(&self, job_id: String) -> Result<JobLogReceiver, ClientError>;
}

pub(crate) struct CommanderClient {
    client: OrchestratorServiceClient<Channel>,
}

impl CommanderClient {
    pub(crate) async fn connect(addr: &str) -> Result<Self, ClientError> {
        let client = OrchestratorServiceClient::connect(addr.to_string()).await?;

        Ok(Self { client })
    }
}

#[async_trait]
impl CommanderClientApi for CommanderClient {
    async fn submit_job(&self, yaml_payload: String) -> Result<String, ClientError> {
        let response = self
            .client
            .clone()
            .submit_job(SubmitJobRequest { yaml_payload })
            .await?;

        Ok(response.into_inner().job_id)
    }

    async fn monitor_job(&self, job_id: String) -> Result<JobLogReceiver, ClientError> {
        let mut stream = self
            .client
            .clone()
            .monitor_job(MonitorJobRequest { job_id })
            .await?
            .into_inner();
        let (tx, rx) = mpsc::unbounded_channel();

        tokio::spawn(async move {
            while let Some(message) = stream.next().await {
                match message {
                    Ok(msg) => {
                        if !msg.log.is_empty() {
                            let prefix = if msg.is_error { "[ERR] " } else { "[OUT] " };
                            let _ = tx.send(format!("{}{}", prefix, msg.log));
                        }

                        if msg.is_final {
                            let _ = tx.send("-- Job has ended --".to_string());
                            break;
                        }
                    }
                    Err(_) => {
                        let _ = tx.send("[FATAL] Stream error while receiving logs!".to_string());
                        break;
                    }
                }
            }
        });

        Ok(rx)
    }
}
