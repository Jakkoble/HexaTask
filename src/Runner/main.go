package main

import (
	"context"
	"log"
	"os"
	"time"

	"github.com/Jakkoble/HexaTask/src/Runner/internal"
	"github.com/Jakkoble/HexaTask/src/Runner/pb"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
)

//go:generate protoc --go_out=pb --go_opt=paths=source_relative --go-grpc_out=pb --go-grpc_opt=paths=source_relative -I ../../contracts ../../contracts/runner.proto

func main() {
	jobID := os.Getenv("JOB_ID")
	commanderURL := os.Getenv("COMMANDER_URL")

	if jobID == "" || commanderURL == "" {
		log.Fatalf("FATAL: Missing environment variables. JOB_ID or COMMANDER_URL is empty.")
	}

	log.Printf("Runner startet for Job ID: %s", jobID)
	log.Printf("Connecting to Commander at: %s", commanderURL)

	conn, err := grpc.NewClient(commanderURL, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("Failed to connect to Commander: %v", err)
	}

	defer conn.Close()

	client := pb.NewRunnerServiceClient(conn)

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	req := &pb.GetJobDetailsRequest{JobId: jobID}

	log.Println("Asking Commander for job details...")
	res, err := client.GetJobDetails(ctx, req)
	if err != nil {
		log.Fatalf("Failed to get job details: %v", err)
	}

	log.Printf("Received %d commands to execute.", len(res.Commands))
	for _, cmd := range res.Commands {
		log.Printf("  -> %s", cmd)
	}

	e := internal.NewTaskExecutor(client, jobID)
	e.ExecuteAndStream(ctx, res.Commands)
}
