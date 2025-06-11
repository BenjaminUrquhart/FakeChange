#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <unistd.h>
#include <errno.h>
#include <sys/stat.h>
#include <sys/syscall.h>
#include <linux/limits.h>

#define byte char

// Use of direct syscalls is to bypass libTAS filesystem hooking

__attribute__((constructor))
void init() {
	FILE* file = fopen("/proc/self/cmdline", "rb");
	if(file) {
		int index = 0;
		char chr;
		while(fread(&chr, sizeof(char), 1, file) && chr) index++;
		
		char cmd[index + 1];
		fseek(file, 0, SEEK_SET);
		fread(cmd, sizeof(char), index + 1, file);
		fclose(file);

		//fprintf(stderr, "%s\n", cmd);

		if(!strstr(cmd, "runner")) {
			fprintf(stderr, "Ignoring non-runner process %s\n", cmd);
			return;
		}
	}
	else fprintf(stderr, "Failed to read cmdline\n");

	struct stat stats;
	//int status = lstat("busy", &stats);
	//fprintf(stderr, "busy status: %d\n", status);
	//if(!status) return;

	char cwd[PATH_MAX];
	getcwd(cwd, PATH_MAX);
	fprintf(stderr, "Working directory: %s\n", cwd);
	
	file = fopen("chapter0/game.unx", "rb");
	if(!file) {
		perror("fopen()");
		fprintf(stderr, "Data file not found.");
		exit(1);
		//return;
	}
	// Get pointer to game save folder name
	fseek(file, 0x14, SEEK_SET);
	int string_ptr;
	fread(&string_ptr, sizeof(int), 1, file);

	// Read save folder name
	fseek(file, string_ptr - 4, SEEK_SET);
	int string_len;
	fread(&string_len, sizeof(int), 1, file);
	char game_name[string_len + 1];
	fread(game_name, sizeof(char), string_len + 1, file);
	fclose(file);
	
	fprintf(stderr, "Save folder name: %s\n", game_name);

	// Construct save folder path
	char* home = getenv("HOME");
	char* base_path = "/.config/";
	char* request_filename = "/chapter";
	char request_chapter_file[strlen(home) + strlen(base_path) + string_len + strlen(request_filename) + 1];
	request_chapter_file[0] = 0;
	strcat(request_chapter_file, home);
	strcat(request_chapter_file, base_path);
	strcat(request_chapter_file, game_name);
	strcat(request_chapter_file, request_filename);
	fprintf(stderr, "Request file: %s\n", request_chapter_file);

	// Check which chapter should be loaded (if any)
	file = fopen(request_chapter_file, "rb");
	
	byte chapter = 0;
	if(file) {
		fread(&chapter, sizeof(byte), 1, file);
		fclose(file);
		syscall(SYS_unlink, request_chapter_file);

		// I'm not dealing with this lol
		if(chapter > 9 || chapter < 0) chapter = 0;
	}
	else {
		fprintf(stderr, "No chapter requested, launching chapter select instead.\n");
	}

	char dest[11];
	strcpy(dest, "./chapter0");
	dest[9] = chapter + '0';
	fprintf(stderr, "Target: %s\n", dest);
	
	int status = lstat("assets", &stats);

	if(!status && !S_ISLNK(stats.st_mode)) {
		fprintf(stderr, "regular assets folder detected, please remove or rename it\n");
		exit(1);
		return;
	}
	else if(status) perror("stat()");
	else if(status = syscall(SYS_unlink, "assets")) perror("syscall(SYS_unlink, \"assets\")");

	if(status = symlink(dest, "./assets")) perror("symlink(assets)");
	if(status = symlink("../mus", "./assets/mus")) perror("symlink(mus)");
	if(status = symlink("../lib/libfakechange.so", "./assets/libfakechange.so")) perror("symlink(libfakechange.so)");
}
