#include "FakeChange.h"
#include <stdbool.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <unistd.h>
#include <errno.h>

extern void write_parameters(char* basepath, char* params) {
	int baselen = strlen(basepath);
	char* path = (char*)malloc((baselen + 8) * sizeof(char)); // len("/params")  -> 7
	sprintf(path, "%s/params", basepath);
	FILE* file = fopen(path, "wb");
	
	if(!file) {
		free(path);
		perror("file()");
		return;
	}
	
	char* args = params;
	char* eof = '\0';
	bool skip = false;
	while(*args) {
		if(!skip && *args == '\\') {
			skip = true;
		}
		else if(!skip && *args == ' ') {
			fwrite(&eof, sizeof(char), 1, file);
		}
		else {
			fwrite(args, sizeof(char), 1, file);
			skip = false;
		}
		args++;
	}
	
	fclose(file);
	free(path);	
}

extern double libtas_is_loaded() {
	if(getenv("LIBTAS_START_FRAME")) return 1;
	
	char* env = getenv("LD_PRELOAD");
	if(env) {
		//printf("ENV: %x\n", env);
		printf("Preload: %s\n", env);
		return (double)(strstr(env, "libtas") != NULL);
	}
	return 0;
}

extern void fake_change(char* basepath, double chapter, char* params) {
	int baselen = strlen(basepath);
	char* path = (char*)malloc((baselen + 9) * sizeof(char)); // len("/chapter") -> 8
	sprintf(path, "%s/chapter", basepath);
	write_parameters(basepath, params);

	char chapterInt = (char)chapter;
	printf("%s %d\n", path, chapterInt);

	FILE* file = fopen(path, "wb");
	if(!file) {
		free(path);
		perror("fopen()");
		return;
	}
	free(path);
	fwrite(&chapterInt, sizeof(char), 1, file);
	fclose(file);
	
	if(libtas_is_loaded()) {
		fprintf(stderr, "libTAS detected, not restarting runtime.\n");
		return;
	}
	
	int pid = fork();
	if(pid) {
		fprintf(stderr, "Started new process %d\n", pid);
		return;
	}
	fprintf(stderr, "Restarting runtime...\n");
	char* argv[] = { "runner", NULL };
	execv("./runner", argv);
	perror("execv()");
	exit(1);
}
