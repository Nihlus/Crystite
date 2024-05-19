echo === INSATLLING/UPDATING RESONITE ===
/usr/games/steamcmd \
	+force_install_dir /var/lib/crystite/Resonite \
	+login $STEAM_CRED $STEAM_PASS \
	+app_license_request 2519830 \
	+app_update 2519830 validate \
	+quit

echo === PURGING OLD CACHE AND LOGS ===
find /var/lib/crystite/Resonite/Data/Assets -type f -atime +7 -delete
find /var/lib/crystite/Resonite/Data/Cache -type f -atime +7 -delete
find /Logs -type f -name *.log -atime +30 -delete

echo === CONVERTING HEADLESS CONFIG FILE ===
RESONITE_CONFIG=$(grep -v " null," "/Config/Config.json")
echo "{ \"Resonite\": $RESONITE_CONFIG }" > /etc/crystite/conf.d/resonite.json

echo === STARTING CRYSTITE ===
/usr/lib/crystite/crystite