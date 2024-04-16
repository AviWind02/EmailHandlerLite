# EmailHandlerLite

EmailHandlerLite is a lightweight utility designed to process incoming email messages related to password reset requests(In my case). This application fetches and logs emails but does not handle the actual password reset logic(Can be added but its not public), which should be handled by a separate system.

## Features

- Fetches emails from a Gmail account using the Gmail API.
- Filters emails based on specific criteria (e.g., subject line containing "Password Reset").
- Logs the IDs of processed emails to prevent reprocessing.
- Operates as a standalone module that can be integrated with other systems.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

- .NET Core 3.1 SDK or later
- Google Cloud Platform account with Gmail API enabled

### Installing

1. **Clone the repository**

   ```bash
   git clone https://github.com/yourusername/EmailHandlerLite.git
   cd EmailHandlerLite
