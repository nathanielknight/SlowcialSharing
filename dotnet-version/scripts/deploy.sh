#!/usr/bin/env sh

set -e

# Clean up
rm -f sloshar.tar

# Build image
finch build --network host --platform amd64 --tag "sloshar" --file Dockerfile .

# Export image
# NOTE: Finch does it's own compression; no need to compress the output.
finch save --output sloshar.tar sloshar

# Copy to Druyan
scp sloshar.tar nknight@druyan:/tmp/sloshar.tar
ssh nknight@druyan sudo --user slowcialsharing -- podman --cgroup-manager=cgroupfs load -i /tmp/sloshar.tar
