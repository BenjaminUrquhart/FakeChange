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
	
	gcc -c -fpic ../FakeChange/RunnerPreload.c
	gcc -shared -o lib/libfcpreload.so RunnerPreload.o
	rm RunnerPreload.o

	gcc -c -fpic ../FakeChange/FakeChange.c
	gcc -shared -o lib/libfakechange.so FakeChange.o
	rm FakeChange.o
	cd ..
	
else
	echo "Failed to run patcher"
fi


