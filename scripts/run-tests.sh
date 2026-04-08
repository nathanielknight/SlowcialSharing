#!/bin/bash
set -e

echo "=== Rust unit tests ==="
cd web && cargo test && cd ..

echo ""
echo "=== Python unit tests ==="
cd fetcher && uv run pytest tests/ -v && cd ..

echo ""
echo "=== Integration tests ==="
cd web && DATABASE_PATH=:memory: ./target/debug/slowcial-web &
SERVER_PID=$!
sleep 1
cd ..
hurl --test integration-tests/*.hurl
kill $SERVER_PID 2>/dev/null

echo ""
echo "All tests passed!"
