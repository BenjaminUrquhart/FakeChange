#!/bin/bash
cd DELTARUNE
LD_PRELOAD=./lib/libfcpreload.so LD_LIBRARY_PATH=./lib:$LD_LIBRARY_PATH libTAS
cd ..
