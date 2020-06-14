#!/usr/bin/env python3
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os

""" Bot Configuration """

class DefaultConfig:
    """ Bot Configuration """
    PORT = 3978
    APP_ID = os.environ.get("MicrosoftAppId", "4dc902a2-18a1-472a-9fb9-d538fcb627e3")
    APP_PASSWORD = os.environ.get("MicrosoftAppPassword", "@mm-echo-bot-22499")
