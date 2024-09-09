To use on Raspberry Pi (even a Pi 2 works fine)

Install .Net per https://learn.microsoft.com/en-us/dotnet/iot/deployment

1. git clone https://github.com/phildimond/EnergySuper
2. dotnet publish --self-contained
3. copy built files to a suitable location, eg /lib/systemd/system
      sudo mkdir /lib/systemd/system/EnergySuper
4.  Edit EnergySuper.service to point to the executable
5. Put EnergySuper.service in the right directory
       sudo cp EnergySuper.service /lib/systemd/system
6. sudo systemctl daemon-reload
7. sudo systemctl start EnergySuper
8. sudo systemctl status EnergySuper
9. sudo systemctl stop EnergySuper

