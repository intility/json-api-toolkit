"""Suppress benign warnings from third-party mkdocs plugins so `mkdocs build --strict` only fails on real issues."""

import logging


class _GitRevisionDateNoiseFilter(logging.Filter):
    NOISE = "First revision timestamp is older than last revision timestamp"

    def filter(self, record: logging.LogRecord) -> bool:
        return self.NOISE not in record.getMessage()


def on_startup(*_args, **_kwargs) -> None:
    logging.getLogger().addFilter(_GitRevisionDateNoiseFilter())
