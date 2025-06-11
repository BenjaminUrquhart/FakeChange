#!/bin/bash
cd DELTARUNE
LD_PRELOAD=./lib/libfcpreload.so ./runner
cd ..
