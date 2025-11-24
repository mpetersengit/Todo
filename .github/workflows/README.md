# GitHub Actions Workflows

This directory contains CI/CD workflows for the Todo API project.

## Workflows

### `ci-cd.yml`
Main CI/CD pipeline that runs on every push and pull request:

1. **Test Job**: Builds the solution and runs all unit/integration tests
   - Uses .NET 10.0 SDK
   - Generates code coverage reports
   - Uploads test results as artifacts
   - Creates test summary annotations

2. **Docker Build Job**: Builds and validates the Docker image
   - Creates a multi-stage Docker build
   - Tests the container by starting it and verifying Swagger UI is accessible
   - Uses Docker Buildx for advanced caching

3. **Docker Push Job**: Pushes images to GitHub Container Registry (ghcr.io)
   - Only runs on pushes to main/master branch
   - Tags images with branch name, SHA, and semantic versioning
   - Requires no additional secrets (uses GITHUB_TOKEN)

## Usage

The workflows run automatically on:
- Push to `main`, `master`, or `develop` branches
- Pull requests targeting those branches

## Viewing Results

- Go to the **Actions** tab in your GitHub repository
- Click on a workflow run to see detailed logs
- Test results are displayed as annotations on the workflow summary
- Docker images are available at `ghcr.io/mpetersengit/todo-api`

## Manual Trigger

To manually trigger a workflow run:
1. Go to Actions tab
2. Select the workflow
3. Click "Run workflow"

