defmodule SlowcialsharingWeb.AboutController do
  use SlowcialsharingWeb, :controller

  def index(conn, _params) do
    conn
    |> render(:index)
  end
end
