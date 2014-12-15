# Foreman #

This is a simple program for generating flowcharts for production lines in the game [Factorio](https://www.factorio.com/).

Requires .Net 4.0 or higher and Visual C++ 2012 to run. Also works in mono, but the GUI has a few problems (missing scroll bars etc).

## Installing ##

Ubuntu Trusty users can use my [PPA](https://launchpad.net/~florian-will/+archive/ubuntu/factorio).

Users of other debian-based distros can probably download the dsc, debian.tar.gz and orig.tar.xz files from the "Package Details" page on the PPA website and build a deb package for their distro using debuild, pbuilder or similar tools. First build and install the nlua packages, then build and install Foreman.

Windows users can find a zip archive with precompiled binaries in the [Factorio Thread about Foreman](http://www.factorioforums.com/forum/viewtopic.php?p=57761#p57761). This archive contains a native code dll file, so it won't work on Linux.

Of course, it's possible to compile the sources manually. You need NLua as a dependency. Once you have that, compiling the Foreman.sln solution in MonoDevelop (and possibly also Visual Studio) should work.

## Using ##

When using the Ubuntu package: Start Foreman through the application menu of your desktop environment or issue the "foreman" command in a terminal.

Otherwise, start Foreman.exe – double click it, or if you're on Linux maybe you need to run "mono Foreman.exe" in a terminal.

Foreman tries to detect your Factorio installation, but currently it mostly fails to do so on Windows. On Linux, it will try to find it in ~/factorio and ~/Factorio. If that fails, simply choose the "Factorio" directory using the "Browse" button in the dialog that appears.

Now you can add output nodes to the chart by choosing an output from the menu in the bottom left and clicking "Add Output". Click the newly inserted node to change the amount of items you want to produce. Click "Automatically complete" to add all the required nodes for producing the output items. You can enable/disable speed modules and assembling machines/furnaces for the calculation by ticking/unticking the check boxes.

Drag the chart using the middle mouse button. Scroll using the mouse wheel.
