#!/bin/bash

HOST="http://localhost:5000/api"

get_drones() {
    curl -s "$HOST/drones" | jq
}

get_drone() {
    if [ -z "$2" ]; then
        echo "Usage: $0 get_drone <id>"
        return
    fi
    curl -s "$HOST/drones/$2" | jq
}

post_drone() {
    if [ -z "$2" ]; then
        echo "Usage: $0 post_drone <json_file>"
        return
    fi
    RESPONSE=$(curl -s -X POST -H "Content-Type: application/json" \
        -d @"$2" "$HOST/drones")
    
    echo "$RESPONSE" | jq
}

case $1 in
    get_drones) get_drones ;;
    get_drone) get_drone "$@" ;;
    post_drone) post_drone "$@" ;;
    *) echo "Usage: $0 {get_drones|get_drone|post_drone}" ;;
esac
