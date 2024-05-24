if [ ${STEAM_CRED:-SteamUsername} = "SteamUsername" ] && [ ${STEAM_PASS:-SteamPassword} = "SteamPassword" ]; then
echo "Required Steam credentials have not been set! Are you sure they are in the \".env\" file? (STEAM_CRED and STEAM_PASS)"
exit 1
fi

echo "=== INSTALLING HEADLESS CONFIG FILE ==="
echo "Selected file: Config/${CONFIG_FILE:=Config.json}"
RESONITE_CONFIG=$(grep -v " null," "/Config/$CONFIG_FILE")
echo "{ \"Resonite\": $RESONITE_CONFIG }" > /etc/crystite/conf.d/resonite.json

echo "=== INSATLLING/UPDATING RESONITE ==="
cat >/etc/crystite/conf.d/steamcreds.json <<EOF
{
    "Headless": {
        "resonitePath": "/var/lib/crystite/Resonite",
        "manageResoniteInstallation": true,
        "steamCredential": "$STEAM_CRED",
        "steamPassword": "$STEAM_PASS"
    },
}
EOF
/usr/lib/crystite/crystite --install-only --ignore-version-mismatch

if [ ${MODLOADER:=None} != "None" ]; then
echo "=== INSTALLING MODLOADER ==="
case $MODLOADER in
  RML)
    echo "Selected modloader: ResoniteModLoader"
    mkdir \
        /var/lib/crystite/Resonite/Libraries \
        /var/lib/crystite/Resonite/rml_mods \
        /var/lib/crystite/Resonite/rml_libs \
        /var/lib/crystite/Resonite/rml_config
    wget -O /var/lib/crystite/Resonite/Libraries/ResoniteModLoader.dll "https://github.com/resonite-modding-group/ResoniteModLoader/releases/latest/download/ResoniteModLoader.dll"
    wget -O /var/lib/crystite/Resonite/rml_libs/0Harmony.dll "https://github.com/resonite-modding-group/ResoniteModLoader/releases/latest/download/0Harmony.dll"
    cat >/etc/crystite/conf.d/modloader.json <<EOF
{
    "Resonite": {
        "pluginAssemblies": "/var/lib/crystite/Resonite/Libraries/ResoniteModLoader.dll",
    },
}
EOF
    ;;
  MonkeyLoader)
    echo "Selected modloader: MonkeyLoader"
    ;;
  MonkeyLoaderRML)
    echo "Selected modloader: MonkeyLoader + ResoniteModLoader"
    ;;
  *)
    echo "Unrecognized modloader \"$MODLOADER\", skipping installation."
    ;;
esac
else
echo "No modloader selected, skipping installation."
fi

echo "=== STARTING CRYSTITE ==="
/usr/lib/crystite/crystite