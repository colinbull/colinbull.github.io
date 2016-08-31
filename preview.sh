#!/bin/bash

mono .paket/paket.exe restore
fsharpi generate.fsx

