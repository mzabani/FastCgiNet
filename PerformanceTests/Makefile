MCS=mcs
MONO=mono
LIBPATH=../FastCgiNet/bin/Release
LIBS=FastCgiNet.dll

receive_req_build: ReceiveRequest.exe
	$(MCS) ReceiveRequest.cs -lib:$(LIBPATH) -r:$(LIBS)

receive_req_exec: receive_req_build
	MONO_PATH=$(LIBPATH) $(MONO) --profile ReceiveRequest.exe 10000
