# MicronImg

Create visual banners and headers for MICRON pages.
Convert images into MICRON format for Reticulum networks.

## About

MICRON is a lightweight markup language designed for text rendering in constrained environments, making it perfect for mesh networks and low-bandwidth communications.

MicronImg converts images (from URLs or local files) into text-based art optimized for **MICRON** format. Perfect for creating distinctive banners and headers on your Reticulum pages bringing visual identity to your `.mu` files without CSS, JavaScript, or other unsupported technologies.

Display image-based artwork in Nomad Network and other MICRON-compatible clients while maintaining full compatibility with terminal rendering.

## Features

- Convert images to monospace-friendly text art
- Load images from URLs or local files
- Generate ready-to-use MICRON markup (`.mu` format)

## Usage

### From file path

MicronImg.exe -p <path> [-o <output_file>]
(or just drag and drop a file)

### From URL

MicronImg.exe -u <url> [-o <output_file>]

## sample.mu

### Displayed in NomadNet CLI

<img width="2557" height="976" alt="{579AD8BA-D880-4C7F-B7A9-A2C271E5DBED}" src="https://github.com/user-attachments/assets/1c410b1f-dbc0-4a13-88e2-5d5ac8e57f62" />

### Displayed in Reticulum MeshChatX

<img width="1293" height="621" alt="{74EB913C-AEC6-4BB6-91E7-154BE18B6B7E}" src="https://github.com/user-attachments/assets/e1e42ae4-5021-4a70-883d-40385810ddbd" />
