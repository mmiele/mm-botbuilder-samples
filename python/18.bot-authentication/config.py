#!/usr/bin/env python3
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os

""" Bot Configuration """

class DefaultConfig:
    """ Bot Configuration """

    PORT = 3978
    APP_ID = os.environ.get("MicrosoftAppId", "984a98ff-bf9d-4d41-9d06-89f4cc971050")
    APP_PASSWORD = os.environ.get("MicrosoftAppPassword", "@mm-echo-bot-auth-22499")
    CONNECTION_NAME = os.environ.get("ConnectionName", "GenericOauth")
