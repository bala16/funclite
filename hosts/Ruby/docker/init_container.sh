#!/bin/bash
service ssh start
if [ -d "/home/site/wwwroot" ]; then
  mkdir -p /home/site/wwwroot/app
  mkdir -p /home/site/wwwroot/userFunc
  cp -r /app /home/site/wwwroot
  cp -r /userFunc /home/site/wwwroot
  /bin/bash -c "ruby /home/site/wwwroot/app/server.rb -o 0.0.0.0"
else
  /bin/bash -c "ruby /app/server.rb -o 0.0.0.0"
fi
