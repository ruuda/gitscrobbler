GitScrobbler.exe: GitScrobbler.cs
	mcs -pkg:dotnet -optimize GitScrobbler.cs

clean:
	rm GitScrobbler.exe
