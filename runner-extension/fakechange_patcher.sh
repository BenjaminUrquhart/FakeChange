#!/bin/bash

if ! [ -e DELTARUNE/runner ]; then
	ln runner DELTARUNE/runner
fi

chmod +x Patcher/FakeChangePatcher
if Patcher/FakeChangePatcher DELTARUNE; then
	cd DELTARUNE
	if ! [ -d lib ]; then
		mkdir lib
	fi

	if command -v gcc; then
		echo "Found gcc, compiling libraries..."
		gcc -c -fpic ../FakeChange/RunnerPreload.c
		gcc -shared -o lib/libfcpreload.so RunnerPreload.o
		rm RunnerPreload.o

		gcc -c -fpic ../FakeChange/FakeChange.c
		gcc -shared -o lib/libfakechange.so FakeChange.o
		rm FakeChange.o
	else
		echo "Copying libraries..."
		cp ../FakeChange/*.so lib
	fi
	cd ..
	echo "Done"
	
else
	echo "Failed to run patcher"
fi


