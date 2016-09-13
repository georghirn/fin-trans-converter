CONFIG=Release

all: build

clean:
	rm -Rf src/finTransConverterLib/bin
	rm -Rf src/finTransConverterLib/obj
	rm -Rf src/finTransConverter/bin
	rm -Rf src/finTransConverter/obj

build:
	dotnet restore
	dotnet build -c $(CONFIG) src/finTransConverterLib/project.json
	dotnet build -c $(CONFIG) src/finTransConverter/project.json

rebuild: clean build

install:
	mkdir -p $(DESTDIR)/usr/lib/fin-trans-converter
	install -D ./src/finTransConverter/bin/$(CONFIG)/netcoreapp1.0/FinTransConverter* $(DESTDIR)/usr/lib/fin-trans-converter
	mkdir -p $(DESTDIR)/usr/bin
	install ./scripts/fin-trans-converter $(DESTDIR)/usr/bin

uninstall:
	rm -Rf $(DESTDIR)/usr/lib/fin-trans-converter
	rm -f $(DESTDIR)/usr/bin

.PHONY: all clean build rebuild install uninstall
