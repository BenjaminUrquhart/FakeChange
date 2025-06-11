#!/bin/bash

if ! [ -e DELTARUNE/chapter0/game.unx ]; then
	echo "WARNING: unable to locate launcher data file"
fi

libtas_path=$(which libTAS)

if setcap -v cap_checkpoint_restore+eip $libtas_path > /dev/null; then
	echo "libTAS has the CAP_CHECKPOINT_RESTORE flag which needs to be disabled for FakeChange to work."
	echo "If a password prompt appears below, please enter your user password to remove the flag."
	if ! sudo setcap cap_checkpoint_restore-eip $libtas_path; then
		echo "Failed to remove flag."
		exit
	fi
fi

cd DELTARUNE
LD_PRELOAD=./lib/libfcpreload.so LD_LIBRARY_PATH=./lib:$LD_LIBRARY_PATH libTAS
cd ..
