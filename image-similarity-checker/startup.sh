#!/bin/bash
gunicorn --bind=0.0.0.0:$PORT app:app
chmod +x startup.sh
