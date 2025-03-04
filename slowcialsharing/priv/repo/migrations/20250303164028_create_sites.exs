defmodule Slowcialsharing.Repo.Migrations.CreateSites do
  use Ecto.Migration

  def change do
    create table(:sites) do
      add :name, :string
      add :homepageurl, :string
      add :lastchecked, :utc_datetime

      timestamps(type: :utc_datetime)
    end
  end
end
