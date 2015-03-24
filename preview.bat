@echo off

call .paket\paket restore
call "C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\Fsi.exe" generate.fsx

