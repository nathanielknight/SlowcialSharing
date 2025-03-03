defmodule Slowcialsharing.Repo do
  use Ecto.Repo,
    otp_app: :slowcialsharing,
    adapter: Ecto.Adapters.SQLite3
end
