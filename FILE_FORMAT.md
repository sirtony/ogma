# Ogma File Format

This file describes the binary file format used by Ogma for storing data.

### Compression

Compression is done with Brotli.

## Header
| Field | Type | Size (in bytes) | Comments |
|:------|:-----|:----------------|:---------|
| Magic ID | `byte[4]` | 4 | The magic ID that identifies the file as an Ogma store. |
| Version | `uint16` | 2 | The version number for the file format. Current value is **2**. Backward compatibility is implementation-defined. |

## Payload

| Field | Type | Size (in bytes) | Comments |
|:------|:-----|:----------------|:---------|
| Data | `byte[]` | variable | The compressed data. |
