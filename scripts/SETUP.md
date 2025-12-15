# NYT Wordle Fetcher Setup

This script automatically fetches tomorrow's Wordle answer from the New York Times and updates the `used-words.csv` file.

## Prerequisites

- NYT Digital Subscription (required to access Wordle review articles)
- Node.js 20+
- npm

## Local Setup

1. **Install dependencies:**
   ```bash
   cd scripts
   npm install
   ```

2. **Install Playwright browsers:**
   ```bash
   npx playwright install chromium
   ```

3. **Set up your NYT credentials:**
   ```bash
   # Copy the example file
   cp .env.example .env

   # Edit .env and add your NYT credentials
   # NYT_EMAIL=your-email@example.com
   # NYT_PASSWORD=your-password
   ```

4. **Test the script:**
   ```bash
   node update-used-words.mjs
   ```

## GitHub Actions Setup

The script runs automatically every day at 2:01 AM EST via GitHub Actions.

### Adding Secrets to GitHub

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add these two secrets:

   **Secret 1:**
   - Name: `NYT_EMAIL`
   - Value: Your NYT account email

   **Secret 2:**
   - Name: `NYT_PASSWORD`
   - Value: Your NYT account password

### How It Works

1. GitHub Actions runs at 2:01 AM EST daily
2. Script logs into NYT using your credentials (stored securely in GitHub Secrets)
3. Fetches tomorrow's Wordle answer from the NYT review article
4. Updates `used-words.csv` if a new word is found
5. Commits and pushes the change automatically

### Manual Trigger

You can also run the workflow manually:
1. Go to **Actions** tab in GitHub
2. Select **Update Wordle Used Words**
3. Click **Run workflow**

## Security Notes

- **Never commit your `.env` file** - it's already in `.gitignore`
- GitHub Secrets are encrypted and never exposed in logs
- The script only has access to secrets when running
- You can revoke/change your password anytime in your NYT account

## Troubleshooting

### "NYT_EMAIL and NYT_PASSWORD environment variables are required"
- Locally: Make sure you created the `.env` file with your credentials
- GitHub Actions: Make sure you added both secrets in repository settings

### "Login failed" or timeout errors
- Verify your NYT credentials are correct
- Check if NYT changed their login page structure
- Try running with `headless: false` locally to see what's happening

### "Could not find answer on NYT page"
- The page structure might have changed
- The article might not be published yet (NYT posts early but timing varies)
- Check the URL manually in a browser to verify it exists
