#!/bin/bash

# Github repository URL
REPO_URL="https://github.com/dipeshdeula/EcommerceIn.git"
DEPLOY_DIR="$HOME/EcommerceIn"
GITHUB_TOKEN=$1
GITHUB_USERNAME=$2

echo "Starting deployment script..."

# Create or clean deployment directory
if [ -d "$DEPLOY_DIR" ]; then
  echo "Updating existing repository..."
  cd "$DEPLOY_DIR"
  git fetch --all
  git reset --hard origin/main
else
  echo "Cloning repository..."
  git clone "$REPO_URL" "$DEPLOY_DIR"
  cd "$DEPLOY_DIR"
fi

# Login to GitHub Container Registry
echo "Logging into GitHub Container Registry..."
echo "$GITHUB_TOKEN" | docker login ghcr.io -u "$GITHUB_USERNAME" --password-stdin

# Bring down existing containers
echo "Stopping existing containers..."
docker-compose down || true

# Pull latest images
echo "Pulling latest images..."
docker-compose pull

# Start containers
echo "Starting containers..."
docker-compose up -d

# Clean up unused images
echo "Cleaning up..."
docker image prune -f

echo "Deployment complete!"