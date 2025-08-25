# Support Ticket System - Backend API

Enterprise support ticket management system with AI integration built with .NET 8.

## Setup

### Prerequisites
- Docker
- Git

### Quick Start

1. Clone repository:

2. Fix Docker permissions (Linux/macOS):

 ```bash
 sudo usermod -aG docker $USER
 newgrp docker
 ```
3. Run application
```bash
docker compose up --build
```
4. Access API:
```bash
API: http://localhost:5104
Documentation: http://localhost:5104/swagger
```