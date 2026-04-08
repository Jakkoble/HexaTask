package internal

import (
	"bufio"
	"context"
	"io"
	"os/exec"

	"github.com/Jakkoble/HexaTask/src/Runner/pb"
)

type TaskExecutor struct {
	client pb.RunnerServiceClient
	jobID  string
}

func NewTaskExecutor(client pb.RunnerServiceClient, jobID string) *TaskExecutor {
	return &TaskExecutor{client: client, jobID: jobID}
}

func (executor *TaskExecutor) ExecuteAndStream(ctx context.Context, commands []string) int {
	stream, err := executor.client.StreamLogs(ctx)
	if err != nil {
		return 1
	}

	var exitCode = 0

	for _, cmdStr := range commands {
		cmd := exec.CommandContext(ctx, "sh", "-c", cmdStr)

		stdout, _ := cmd.StdoutPipe()
		stderr, _ := cmd.StderrPipe()
		multi := io.MultiReader(stdout, stderr)

		if err := cmd.Start(); err != nil {
			executor.sendLog(stream, "Failed to start: "+err.Error(), true)
			return 1
		}

		scanner := bufio.NewScanner(multi)
		for scanner.Scan() {
			executor.sendLog(stream, scanner.Text(), false)
		}

		if err := cmd.Wait(); err != nil {
			if exitErr, ok := err.(*exec.ExitError); ok {
				exitCode = exitErr.ExitCode()
			} else {
				exitCode = 1
			}

			executor.sendLog(stream, "Command failed", true)
			break
		}
	}

	stream.CloseAndRecv()
	return exitCode
}

func (executor *TaskExecutor) sendLog(stream pb.RunnerService_StreamLogsClient, msg string, isError bool) {
	stream.Send(&pb.LogMessage{
		JobId:   executor.jobID,
		Log:     msg,
		IsError: isError,
	})
}
