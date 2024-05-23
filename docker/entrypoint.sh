if [ ${STEAM_CRED:-SteamUsername} = "SteamUsername" ] && [ ${STEAM_PASS:-SteamPassword} = "SteamPassword" ]; then
echo "Required Steam credentials have not been set! Are you sure they are in the \".env\" file? (STEAM_CRED and STEAM_PASS)"
exit 1
fi

echo "=== INSATLLING/UPDATING RESONITE ==="
if [ -v HEADLESS_KEY ]; then
/usr/games/steamcmd \
    +force_install_dir /var/lib/crystite/Resonite \
    +login $STEAM_CRED $STEAM_PASS \
    +app_license_request 2519830 \
    +app_update 2519830 -beta headless -betapassword $HEADLESS_KEY validate \
    +quit
else
/usr/games/steamcmd \
    +force_install_dir /var/lib/crystite/Resonite \
    +login $STEAM_CRED $STEAM_PASS \
    +app_license_request 2519830 \
    +app_update 2519830 validate \
    +quit
fi

echo "=== INSTALLING HEADLESS CONFIG FILE ==="
echo "Selected file: Config/${CONFIG_FILE:=Config.json}"
RESONITE_CONFIG=$(grep -v " null," "/Config/$CONFIG_FILE")
echo "{ \"Resonite\": $RESONITE_CONFIG }" > /etc/crystite/conf.d/resonite.json

echo "=== STARTING CRYSTITE ==="
/usr/lib/crystite/crystite