To use...

1. Edit EnergySuper.service to point to the executable if necessary
2. dotnet publish
2. sudo cp EnergySuper.service /lib/systemd/system
3. sudo systemctl daemon-reload
3. sudo systemctl start EnergySuper
4. sudo systemctl status EnergySuper
5. sudo systemctl stop EnergySuper

For production copy the publish directory contents to a suitable 
location, eg /usr/sbin, modify the .service file to point to that, and run from there.

sudo cp /home/phillip/source/c#/EnergySuper/EnergySuper/bin/Release/net8.0/linux-x64/publish/* /usr/sbin/EnergySuper/
