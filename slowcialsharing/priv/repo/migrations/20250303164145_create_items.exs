defmodule Slowcialsharing.Repo.Migrations.CreateItems do
  use Ecto.Migration

  def change do
    create table(:items) do
      add :key, :string
      add :title, :string
      add :link, :string
      add :pubdate, :utc_datetime
      add :commentslink, :string
      add :score, :integer
      add :comments, :integer
      add :site_id, references(:sites, on_delete: :nothing)

      timestamps(type: :utc_datetime)
    end

    create unique_index(:items, [:key])
    create index(:items, [:site_id])
  end
end
