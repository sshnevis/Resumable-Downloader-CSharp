# Exir Downloader

A simple Windows Forms application to download and install the latest version of Exir software.

## Features

- Downloads a ZIP file from a specified URL.
- Resumes partially downloaded files.
- Shows download progress in the UI.
- Automatically extracts the downloaded file.
- Runs the setup file after extraction.

## How It Works

1. Click the **Download** button.
2. The application downloads the latest Exir ZIP package from: https://exirmatab.com/uploads/Exir-latest.zip
3. After downloading:
- If not already extracted, the ZIP file is extracted.
- The `ExirSetup.exe` installer is launched.
4. You can optionally open the installer directly after download.

## Requirements

- .NET Framework (suitable for running Windows Forms applications)
- Internet access

## Notes

- If the file has already been partially downloaded, the application will continue from where it left off.
- The download can be canceled by closing the form.

## License

This project is proprietary to Exir Matab. All rights reserved.
