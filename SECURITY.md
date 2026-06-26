# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability within Noor, please send an email to the project maintainer. All security vulnerabilities will be promptly addressed.

**Please do NOT report security vulnerabilities through public GitHub issues.**

### What to include

- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

### Response timeline

- **Acknowledgment**: Within 48 hours
- **Initial assessment**: Within 1 week
- **Fix or mitigation**: Within 2 weeks for critical issues

## Security Best Practices

This application stores user settings locally. No sensitive data (passwords, tokens) should ever be stored in configuration files.

### Data & Privacy

- The only network request the app makes by default is **IP geolocation** to estimate your city/timezone,
  over **HTTPS** (`https://ipwho.is/`). No personal data is sent to any Noor-operated server — there is none.
- You can disable network lookups entirely by entering your location manually in **Settings**.
- All settings are stored as plain-text JSON under your user data directory
  (`%LOCALAPPDATA%\Noor\settings.json` on Windows, `~/.local/share/Noor/settings.json` on Linux).
  Do not store anything secret there.

## Contact

For security-related inquiries, please open a **private security advisory** via the GitHub
"Security" tab, or contact the maintainer directly.
