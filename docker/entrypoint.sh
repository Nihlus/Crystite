if [ ${STEAM_CRED:-SteamUsername} = "SteamUsername" ] && [ ${STEAM_PASS:-SteamPassword} = "SteamPassword" ]; then
    echo "Required Steam credentials have not been set! Are you sure they are in the \".env\" file? (STEAM_CRED and STEAM_PASS)"
    exit 1
fi

echo "=== INSTALLING HEADLESS CONFIG FILE ==="
echo "Selected file: Config/${CONFIG_FILE:=Config.json}"
RESONITE_CONFIG=$(grep -v " null," "/Config/$CONFIG_FILE")
echo "{ \"Resonite\": $RESONITE_CONFIG }" > /etc/crystite/conf.d/resonite.json
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

if [ ! -d /var/lib/crystite/Resonite ] || [ ! -e /var/lib/crystite/Resonite/Resonite.x86_64 ]; then
echo "=== INSATLLING RESONITE ==="
/usr/lib/crystite/crystite --install-only --allow-unsupported-resonite-version
fi

# test -e /var/lib/crystite/Resonite/modloader && LAST_MODLOADER=$(cat /var/lib/crystite/Resonite/modloader)
# if [ ${MODLOADER:=None} != ${LAST_MODLOADER:=None} ]; then
# echo "=== CLEANING UP PREVIOUS MODLOADER ==="
# case $LAST_MODLOADER in
#   RML)
#     test -e /etc/crystite/conf.d/modloader.json && rm /etc/crystite/conf.d/modloader.json
#     # This probably isn't necessarry but lets do it anyway
#     rm /var/lib/crystite/Resonite/Libraries/ResoniteModLoader.dll /var/lib/crystite/Resonite/rml_libs/0Harmony.dll
#     ;;
#   MonkeyLoader|MonkeyLoaderRML)
    
#     ;;
# esac
# fi
# echo $MODLOADER > /var/lib/crystite/Resonite/modloader

if [ $MODLOADER != "None" ]; then
echo "=== INSTALLING MODLOADER ==="
case $MODLOADER in
  ResoniteModLoader|RML)
    echo "Selected modloader: ResoniteModLoader"
    mkdir \
        /var/lib/crystite/Resonite/Libraries \
        /var/lib/crystite/Resonite/rml_mods \
        /var/lib/crystite/Resonite/rml_libs \
        /var/lib/crystite/Resonite/rml_config
    wget -O /var/lib/crystite/Resonite/Libraries/ResoniteModLoader.dll "https://github.com/resonite-modding-group/ResoniteModLoader/releases/latest/download/ResoniteModLoader.dll"
    test ! -e /var/lib/crystite/Resonite/Libraries/ResoniteModLoader.dll && echo "Failed to download ResoniteModLoader.dll!" && exit 1
    wget -O /var/lib/crystite/Resonite/rml_libs/0Harmony.dll "https://github.com/resonite-modding-group/ResoniteModLoader/releases/latest/download/0Harmony.dll"
    test ! -e /var/lib/crystite/Resonite/rml_libs/0Harmony.dll && echo "Failed to download 0Harmony.dll!" && exit 1
    cat >/etc/crystite/conf.d/modloader.json <<EOF
{
    "Resonite": {
        "pluginAssemblies": ["/var/lib/crystite/Resonite/Libraries/ResoniteModLoader.dll"],
    },
}
EOF
    ;;
  MonkeyLoader)
    echo "Selected modloader: MonkeyLoader"
    echo "Not implemented" && exit 1
    ML_URL=$(wget -O- https://api.github.com/repos/ResoniteModdingGroup/MonkeyLoader.GamePacks.Resonite/releases/latest | jq -r '.assets[] | select(.name|endswith(".zip")) | .browser_download_url')
    test ! -v ML_URL && echo "Failed to get MonkeyLoader download URL!" && exit 1
    wget -O /var/lib/crystite/MonkeyLoader.zip "$ML_URL"
    test ! -e /var/lib/crystite/MonkeyLoader.zip && echo "Failed to download MonkeyLoader.zip!" && exit 1
    unzip -o /var/lib/crystite/MonkeyLoader.zip -d /var/lib/crystite/Resonite
    rm /var/lib/crystite/MonkeyLoader.zip /var/lib/crystite/Resonite/MonkeyLoader/GamePacks/MonkeyLoader.GamePacks.ResoniteModLoader.nupkg
    ;;
  MonkeyLoaderRML)
    echo "Selected modloader: MonkeyLoader + ResoniteModLoader"
    echo "Not implemented" && exit 1
    ML_URL=$(wget -O- https://api.github.com/repos/ResoniteModdingGroup/MonkeyLoader.GamePacks.Resonite/releases/latest | jq -r '.assets[] | select(.name|endswith(".zip")) | .browser_download_url')
    test ! -v ML_URL && echo "Failed to get MonkeyLoader download URL!" && exit 1
    wget -O /var/lib/crystite/MonkeyLoader.zip "$ML_URL"
    test ! -e /var/lib/crystite/MonkeyLoader.zip && echo "Failed to download MonkeyLoader.zip!" && exit 1
    unzip -o /var/lib/crystite/MonkeyLoader.zip -d /var/lib/crystite/Resonite
    rm /var/lib/crystite/MonkeyLoader.zip
    # rm /var/lib/crystite/MonkeyLoader.zip /var/lib/crystite/Resonite/MonkeyLoader/GamePacks/MonkeyLoader.GamePacks.ResoniteModLoader
    # wget -O /var/lib/crystite/Resonite/MonkeyLoader/GamePacks/MonkeyLoader.GamePacks.ResoniteModLoader "https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.ResoniteModLoader/releases/latest/download/MonkeyLoader.GamePacks.ResoniteModLoader.nupkg"
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