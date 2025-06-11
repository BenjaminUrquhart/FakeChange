!! KEEP THE RUNNER NAMED "runner" THIS IS WHAT THE INJECTED LIBRARY LOOKS FOR TO MAKE SURE IT DOESN'T INTERFERE WITH OTHER PROCESSES !!

How to use:
1 - Place your DELTARUNE files in a folder called "DELTARUNE" inside this folder.
2 - Run fakechange_patcher.sh to apply the data file patches.
3 - Use run.sh to run the game standalone. Use libtas.sh to run the game in libTAS.

You only need to run the patcher once per update.

Most likely, your save files will be within the DELTARUNE folder instead of the normal save location. If you're having trouble with saves not saving or going to the wrong place, this is why.


IMPORTANT: For use in libTAS, you must remove the CAP_CHECKPOINT_RESTORE flag from the libTAS binary as having it enables secure mode for the process which stops me from loading my native library. The provided script should do it for you.

Version History:
- 1.01a:
	- libtas.sh now removes CAP_CHECKPOINT_RESTORE for you.
	- gcc should no longer be required to run the patcher.
	- Better error logging.
- 1.01:
	- Fixed libtas.sh.
	- Preserve command-line arguments when starting chapters.
	- Added basic support for in-place patching for future updates.
- 1.0:
	- Initial release.
