# Ogma File Format

This file describes the binary file format used by Ogma for storing data.

### Compression

Compression is done with Snappy.

### Encryption

Encryption is done with AES-256-GCM and uses the checksum as additional authenticated data.

For key derivation, Argon2id is used with 10 iterations and 20 KiB of memory.

## Header
| Field | Type | Size (in bytes) | Comments |
|:------|:-----|:----------------|:---------|
| Magic ID | `byte[4]` | 4 | The magic ID that identifies the file as an Ogma store. |
| Version | `uint16` | 2 | The version number for the file format. Current value is **1**. |
| Flags | `uint8` | 1 | A bit set of various flags. See below for more details. |

## Flags

| Flag | Bit | Comments |
|:-----|:----|:---------|
| IsEncrypted | 0 | Tells the file parser if the contents of the file have been encrypted. |
| Reserved | 1 | Not currently used. |
| Reserved | 2 | Not currently used. |
| Reserved | 3 | Not currently used. |
| Reserved | 4 | Not currently used. |
| Reserved | 5 | Not currently used. |
| Reserved | 6 | Not currently used. |
| Reserved | 7 | Not currently used. |

## Payload

| Field | Type | Size (in bytes) | Comments |
|:------|:-----|:----------------|:---------|
| Salt | `byte[16]` | 16 | A random salt used for hashing the password during encryption/decryption. Only present when the `IsEncrypted` flag is set. |
| Nonce | `byte[12]` | 12 | A random nonce/IV for encryption and decryption. Only present when the `IsEncrypted` flag is set. |
| Tag | `byte[16]` | 16 | The authentication tag for the AES-256-GCM cipher. Only present when the `IsEncrypted` flag is set. |
| Checksum | `byte[64]` | 64 | A SHA3-512 hash of the raw, uncompressed and unencrypted data. |
| Length | `uint32` | 4 | The length of the following data section. |
| Data | `byte[]` | variable | The compressed and (maybe) encrypted data. |
